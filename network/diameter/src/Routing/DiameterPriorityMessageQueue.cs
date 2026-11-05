using System;
using System.Collections.Generic;

namespace diameter;

internal class DiameterPriorityMessageQueueItem
{
    internal DiameterMessage Message = new DiameterMessage();
    internal DiameterRoutingPriority Priority =
        DiameterRoutingPriority.Priority10;
    internal bool PriorityPresent;
    internal long Sequence;
    internal int BypassCount;
}

internal class DiameterPriorityMessageQueueResult
{
    internal bool Accepted;
    internal bool Found;
    internal bool CapacityReached;
    internal DiameterMessage? Message;
    internal DiameterRoutingPriority Priority =
        DiameterRoutingPriority.Priority10;
    internal bool PriorityPresent;
    internal string Code = string.Empty;
}

internal class DiameterPriorityMessageQueue
{
    internal object QueueLock = new object();
    internal List<DiameterPriorityMessageQueueItem> Items =
        new List<DiameterPriorityMessageQueueItem>();
    internal int Capacity;
    internal int MaximumBypassCount;
    internal DiameterRoutingPriority DefaultPriority;
    internal long NextSequence;

    internal DiameterPriorityMessageQueue(
        int capacity,
        int maximumBypassCount,
        DiameterRoutingPriority defaultPriority)
    {
        int defaultValue = (int)defaultPriority;
        if (capacity <= 0 || maximumBypassCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "Priority queue limits");
        }

        if (defaultValue < (int)DiameterRoutingPriority.Priority0 ||
            defaultValue > (int)DiameterRoutingPriority.Priority15)
        {
            throw new ArgumentOutOfRangeException("defaultPriority");
        }

        Capacity = capacity;
        MaximumBypassCount = maximumBypassCount;
        DefaultPriority = defaultPriority;
    }

    internal DiameterPriorityMessageQueueResult EnqueueRequestMessage(
        DiameterMessage? message)
    {
        DrmpInspectionResult priority =
            DrmpProcessor.InspectRequestPriority(
                message,
                DefaultPriority);
        return EnqueueInspectedMessage(message, priority);
    }

    internal DiameterPriorityMessageQueueResult EnqueueAnswerMessage(
        DiameterMessage? message,
        DiameterRoutingPriority requestPriority)
    {
        DrmpInspectionResult priority =
            DrmpProcessor.InspectAnswerPriority(
                message,
                requestPriority);
        return EnqueueInspectedMessage(message, priority);
    }

    internal DiameterPriorityMessageQueueResult EnqueueInspectedMessage(
        DiameterMessage? message,
        DrmpInspectionResult priority)
    {
        DiameterPriorityMessageQueueResult result =
            new DiameterPriorityMessageQueueResult();
        result.Priority = priority.Priority;
        result.PriorityPresent = priority.Present;
        result.Code = priority.Code;
        if (!priority.Valid || message == null)
        {
            return result;
        }

        lock (QueueLock)
        {
            if (Items.Count >= Capacity)
            {
                result.CapacityReached = true;
                result.Code = "priority-queue-full";
                return result;
            }

            DiameterPriorityMessageQueueItem item =
                new DiameterPriorityMessageQueueItem();
            item.Message = RouteTableCloner.CloneRouteMessage(message);
            item.Priority = priority.Priority;
            item.PriorityPresent = priority.Present;
            item.Sequence = NextSequence;
            NextSequence = NextSequence + 1L;
            Items.Add(item);
            result.Accepted = true;
            result.Code = "priority-message-enqueued";
            return result;
        }
    }

    internal DiameterPriorityMessageQueueResult DequeueMessage()
    {
        lock (QueueLock)
        {
            DiameterPriorityMessageQueueResult result =
                new DiameterPriorityMessageQueueResult();
            if (Items.Count == 0)
            {
                result.Code = "priority-queue-empty";
                return result;
            }

            int selectedIndex = SelectPriorityMessageIndex();
            DiameterPriorityMessageQueueItem selected =
                Items[selectedIndex];
            int index = 0;
            while (index < Items.Count)
            {
                DiameterPriorityMessageQueueItem waiting = Items[index];
                if (index != selectedIndex &&
                    (int)waiting.Priority > (int)selected.Priority &&
                    waiting.BypassCount < MaximumBypassCount)
                {
                    waiting.BypassCount = waiting.BypassCount + 1;
                }

                index = index + 1;
            }

            Items.RemoveAt(selectedIndex);
            result.Found = true;
            result.Message = RouteTableCloner.CloneRouteMessage(
                selected.Message);
            result.Priority = selected.Priority;
            result.PriorityPresent = selected.PriorityPresent;
            result.Code = selected.BypassCount >= MaximumBypassCount ?
                "priority-message-starvation-prevented" :
                "priority-message-dequeued";
            return result;
        }
    }

    internal int SelectPriorityMessageIndex()
    {
        int selectedIndex = -1;
        int index = 0;
        while (index < Items.Count)
        {
            DiameterPriorityMessageQueueItem item = Items[index];
            if (item.BypassCount >= MaximumBypassCount &&
                (selectedIndex < 0 ||
                Items[selectedIndex].BypassCount < MaximumBypassCount ||
                item.Sequence < Items[selectedIndex].Sequence))
            {
                selectedIndex = index;
            }

            index = index + 1;
        }

        if (selectedIndex >= 0)
        {
            return selectedIndex;
        }

        selectedIndex = 0;
        index = 1;
        while (index < Items.Count)
        {
            DiameterPriorityMessageQueueItem item = Items[index];
            DiameterPriorityMessageQueueItem selected =
                Items[selectedIndex];
            if ((int)item.Priority < (int)selected.Priority ||
                (item.Priority == selected.Priority &&
                item.Sequence < selected.Sequence))
            {
                selectedIndex = index;
            }

            index = index + 1;
        }

        return selectedIndex;
    }

    internal int ReadMessageCount()
    {
        lock (QueueLock)
        {
            return Items.Count;
        }
    }
}
