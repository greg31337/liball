using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EventSourceResponsePump
{
    internal EventSourceHub Hub;
    internal EventSourceSerializer Serializer = new EventSourceSerializer();

    internal EventSourceResponsePump(EventSourceHub hub)
    {
        if (hub == null)
        {
            throw new ArgumentNullException("hub");
        }

        Hub = hub;
    }

    internal void Start(
        EventSourceSubscriber subscriber,
        Stream output,
        CancellationToken cancellationToken)
    {
        Task pumping = Task.Run(
            () => PumpEvents(subscriber, output, cancellationToken),
            cancellationToken);
        pumping.ContinueWith(
            task => CompletePump(task, subscriber, output),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    internal void PumpEvents(
        EventSourceSubscriber subscriber,
        Stream output,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            EventSourceReadResult result = Hub.ReadNext(subscriber, cancellationToken);
            if (result.IsComplete)
            {
                return;
            }

            if (result.HasEvent && result.Event != null)
            {
                byte[] bytes = Serializer.Serialize(result.Event);
                output.Write(bytes, 0, bytes.Length);
                output.Flush();
            }
        }
    }

    internal void CompletePump(
        Task pumping,
        EventSourceSubscriber subscriber,
        Stream output)
    {
        if (pumping.IsFaulted)
        {
            AggregateException? failure = pumping.Exception;
            if (failure != null)
            {
                failure.GetBaseException();
            }
        }

        Hub.CloseSubscriber(subscriber);
        output.Dispose();
    }
}
