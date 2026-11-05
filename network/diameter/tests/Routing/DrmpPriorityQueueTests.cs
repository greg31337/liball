namespace diameter.Tests;

public class DrmpPriorityQueueTests
{
    [Test]
    public void RequestInspectionUsesExplicitOrConfiguredDefaultPriority()
    {
        DiameterMessage explicitMessage = CreateMessage(1U);
        explicitMessage.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority3));
        DiameterMessage implicitMessage = CreateMessage(2U);

        DrmpInspectionResult explicitPriority =
            DrmpProcessor.InspectRequestPriority(
                explicitMessage,
                DiameterRoutingPriority.Priority10);
        DrmpInspectionResult defaultPriority =
            DrmpProcessor.InspectRequestPriority(
                implicitMessage,
                DiameterRoutingPriority.Priority6);

        Assert.Multiple(() =>
        {
            Assert.That(explicitPriority.Valid, Is.True);
            Assert.That(explicitPriority.Present, Is.True);
            Assert.That(explicitPriority.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority3));
            Assert.That(explicitPriority.Code,
                Is.EqualTo("drmp-explicit-priority"));
            Assert.That(defaultPriority.Valid, Is.True);
            Assert.That(defaultPriority.Present, Is.False);
            Assert.That(defaultPriority.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority6));
            Assert.That(defaultPriority.Code,
                Is.EqualTo("drmp-default-priority"));
            Assert.That(implicitMessage.Avps, Is.Empty);
        });
    }

    [Test]
    public void AnswerInspectionPrefersAnswerDrmpOverStoredRequestPriority()
    {
        DiameterMessage implicitAnswer = CreateAnswer(3U);
        DiameterMessage explicitAnswer = CreateAnswer(4U);
        explicitAnswer.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority1));

        DrmpInspectionResult inherited =
            DrmpProcessor.InspectAnswerPriority(
                implicitAnswer,
                DiameterRoutingPriority.Priority8);
        DrmpInspectionResult overridden =
            DrmpProcessor.InspectAnswerPriority(
                explicitAnswer,
                DiameterRoutingPriority.Priority8);

        Assert.Multiple(() =>
        {
            Assert.That(inherited.Valid, Is.True);
            Assert.That(inherited.Present, Is.False);
            Assert.That(inherited.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority8));
            Assert.That(overridden.Valid, Is.True);
            Assert.That(overridden.Present, Is.True);
            Assert.That(overridden.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority1));
        });
    }

    [Test]
    public void InspectionRejectsDuplicateVendorSpecificMalformedAndUnknownDrmp()
    {
        DiameterMessage duplicate = CreateMessage(5U);
        duplicate.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority4));
        duplicate.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority5));
        DiameterMessage vendorSpecific = CreateMessage(6U);
        DiameterAvp vendor = CreateDrmpAvp(
            DiameterRoutingPriority.Priority4);
        vendor.Flags = DiameterAvpFlags.VendorSpecific;
        vendor.VendorId = 10415U;
        vendorSpecific.Avps.Add(vendor);
        DiameterMessage malformed = CreateMessage(7U);
        DiameterAvp shortValue = CreateDrmpAvp(
            DiameterRoutingPriority.Priority4);
        shortValue.Data = new byte[] { 0, 0, 4 };
        malformed.Avps.Add(shortValue);
        DiameterMessage unknown = CreateMessage(8U);
        DiameterAvp unknownValue = CreateDrmpAvp(
            DiameterRoutingPriority.Priority4);
        NetworkByteOrder.WriteInt32(unknownValue.Data, 16);
        unknown.Avps.Add(unknownValue);

        Assert.Multiple(() =>
        {
            Assert.That(DrmpProcessor.InspectRequestPriority(
                duplicate,
                DiameterRoutingPriority.Priority10).Code,
                Is.EqualTo("drmp-duplicate"));
            Assert.That(DrmpProcessor.InspectRequestPriority(
                vendorSpecific,
                DiameterRoutingPriority.Priority10).Code,
                Is.EqualTo("drmp-vendor-invalid"));
            Assert.That(DrmpProcessor.InspectRequestPriority(
                malformed,
                DiameterRoutingPriority.Priority10).Code,
                Is.EqualTo("drmp-value-invalid"));
            Assert.That(DrmpProcessor.InspectRequestPriority(
                unknown,
                DiameterRoutingPriority.Priority10).Code,
                Is.EqualTo("drmp-value-invalid"));
        });
    }

    [Test]
    public void QueueIsPriorityOrderedAndStableWithinEachPriority()
    {
        DiameterPriorityMessageQueue queue =
            new DiameterPriorityMessageQueue(
                8,
                8,
                DiameterRoutingPriority.Priority10);
        DiameterMessage normalFirst = CreateMessage(10U);
        DiameterMessage normalSecond = CreateMessage(11U);
        DiameterMessage highFirst = CreateMessage(12U);
        highFirst.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority2));
        DiameterMessage highSecond = CreateMessage(13U);
        highSecond.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority2));

        queue.EnqueueRequestMessage(normalFirst);
        queue.EnqueueRequestMessage(normalSecond);
        queue.EnqueueRequestMessage(highFirst);
        queue.EnqueueRequestMessage(highSecond);
        normalFirst.HopByHopIdentifier = 99U;

        DiameterPriorityMessageQueueResult first = queue.DequeueMessage();
        DiameterPriorityMessageQueueResult second = queue.DequeueMessage();
        DiameterPriorityMessageQueueResult third = queue.DequeueMessage();
        DiameterPriorityMessageQueueResult fourth = queue.DequeueMessage();

        Assert.Multiple(() =>
        {
            Assert.That(first.Message!.HopByHopIdentifier, Is.EqualTo(12U));
            Assert.That(second.Message!.HopByHopIdentifier, Is.EqualTo(13U));
            Assert.That(third.Message!.HopByHopIdentifier, Is.EqualTo(10U));
            Assert.That(fourth.Message!.HopByHopIdentifier, Is.EqualTo(11U));
            Assert.That(first.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority2));
            Assert.That(third.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority10));
            Assert.That(queue.ReadMessageCount(), Is.Zero);
        });
    }

    [Test]
    public void QueueOrdersEveryRfc7944PriorityFromZeroThroughFifteen()
    {
        DiameterPriorityMessageQueue queue =
            new DiameterPriorityMessageQueue(
                16,
                32,
                DiameterRoutingPriority.Priority10);
        int priority = 15;
        while (priority >= 0)
        {
            DiameterMessage message = CreateMessage((uint)(100 + priority));
            message.Avps.Add(CreateDrmpAvp(
                (DiameterRoutingPriority)priority));
            DiameterPriorityMessageQueueResult accepted =
                queue.EnqueueRequestMessage(message);
            Assert.That(accepted.Accepted, Is.True);
            priority = priority - 1;
        }

        priority = 0;
        while (priority <= 15)
        {
            DiameterPriorityMessageQueueResult selected =
                queue.DequeueMessage();
            Assert.Multiple(() =>
            {
                Assert.That(selected.Found, Is.True);
                Assert.That((int)selected.Priority, Is.EqualTo(priority));
                Assert.That(selected.Message!.HopByHopIdentifier,
                    Is.EqualTo((uint)(100 + priority)));
                Assert.That(selected.PriorityPresent, Is.True);
            });
            priority = priority + 1;
        }

        Assert.That(queue.ReadMessageCount(), Is.Zero);
    }

    [Test]
    public void QueueBoundsPriorityBypassesToPreventStarvation()
    {
        DiameterPriorityMessageQueue queue =
            new DiameterPriorityMessageQueue(
                8,
                3,
                DiameterRoutingPriority.Priority10);
        DiameterMessage low = CreateMessage(20U);
        low.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority15));
        queue.EnqueueRequestMessage(low);

        int index = 0;
        while (index < 3)
        {
            DiameterMessage high = CreateMessage((uint)(30 + index));
            high.Avps.Add(CreateDrmpAvp(
                DiameterRoutingPriority.Priority0));
            queue.EnqueueRequestMessage(high);
            DiameterPriorityMessageQueueResult selected =
                queue.DequeueMessage();
            Assert.That(selected.Message!.HopByHopIdentifier,
                Is.EqualTo((uint)(30 + index)));
            index = index + 1;
        }

        DiameterMessage continuedHigh = CreateMessage(40U);
        continuedHigh.Avps.Add(CreateDrmpAvp(
            DiameterRoutingPriority.Priority0));
        queue.EnqueueRequestMessage(continuedHigh);
        DiameterPriorityMessageQueueResult protectedLow =
            queue.DequeueMessage();
        DiameterPriorityMessageQueueResult remainingHigh =
            queue.DequeueMessage();

        Assert.Multiple(() =>
        {
            Assert.That(protectedLow.Message!.HopByHopIdentifier,
                Is.EqualTo(20U));
            Assert.That(protectedLow.Code,
                Is.EqualTo("priority-message-starvation-prevented"));
            Assert.That(remainingHigh.Message!.HopByHopIdentifier,
                Is.EqualTo(40U));
        });
    }

    [Test]
    public void QueueUsesRequestPriorityForAnswersAndReportsCapacity()
    {
        DiameterPriorityMessageQueue queue =
            new DiameterPriorityMessageQueue(
                1,
                4,
                DiameterRoutingPriority.Priority10);
        DiameterPriorityMessageQueueResult accepted =
            queue.EnqueueAnswerMessage(
                CreateAnswer(50U),
                DiameterRoutingPriority.Priority5);
        DiameterPriorityMessageQueueResult full =
            queue.EnqueueRequestMessage(CreateMessage(51U));
        DiameterPriorityMessageQueueResult dequeued =
            queue.DequeueMessage();
        DiameterPriorityMessageQueueResult empty =
            queue.DequeueMessage();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority5));
            Assert.That(accepted.PriorityPresent, Is.False);
            Assert.That(full.Accepted, Is.False);
            Assert.That(full.CapacityReached, Is.True);
            Assert.That(full.Code, Is.EqualTo("priority-queue-full"));
            Assert.That(dequeued.Priority,
                Is.EqualTo(DiameterRoutingPriority.Priority5));
            Assert.That(empty.Found, Is.False);
            Assert.That(empty.Code, Is.EqualTo("priority-queue-empty"));
        });
    }

    private static DiameterMessage CreateMessage(uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request;
        result.CommandCode = 300U;
        result.ApplicationId = 4U;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100U;
        return result;
    }

    private static DiameterMessage CreateAnswer(uint identifier)
    {
        DiameterMessage result = CreateMessage(identifier);
        result.Flags = DiameterMessageFlags.None;
        return result;
    }

    private static DiameterAvp CreateDrmpAvp(
        DiameterRoutingPriority priority)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = DiameterAvpCodes.Drmp;
        result.Data = new byte[4];
        NetworkByteOrder.WriteInt32(result.Data, (int)priority);
        return result;
    }
}
