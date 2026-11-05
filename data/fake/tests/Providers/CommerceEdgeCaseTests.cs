using fake;

namespace fake.Tests;

[TestFixture]
public class CommerceEdgeCaseTests
{
    [Test]
    public void ZeroQuantitiesAreRejectedAndConfiguredLargeQuantitiesAreSupported()
    {
        const long seed = 11120;
        CommerceLineItem large = Fake.Create(seed).Commerce().LineItemFromQuantityAndCurrencyCode(10000, "USD");
        CommerceProvider inspected = Fake.Create(seed).Commerce();
        CommerceProvider untouched = Fake.Create(seed).Commerce();
        FakeOptions limitedOptions = FakeOptions.Create(seed);
        limitedOptions.MaximumCount = 9999;

        Assert.Multiple(() =>
        {
            Assert.That(large.Quantity, Is.EqualTo(10000));
            Assert.That(large.Subtotal, Is.EqualTo(decimal.Round(
                large.UnitPrice * 10000,
                2,
                MidpointRounding.ToEven)));
            Assert.That(large.Total, Is.EqualTo(large.Subtotal));
            Assert.That(() => inspected.LineItemFromQuantityAndCurrencyCode(0, "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => Fake.Create(limitedOptions).Commerce().LineItemFromQuantityAndCurrencyCode(10000, "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void LineRoundingAllocationsReconcileExactlyToOrderTotals()
    {
        CommerceProvider lineProvider = Fake.Create(11121).Commerce();
        CommerceLineItem[] supplied = new CommerceLineItem[]
        {
            lineProvider.LineItemFromQuantityAndCurrencyCode(1, "USD"),
            lineProvider.LineItemFromQuantityAndCurrencyCode(1, "USD"),
            lineProvider.LineItemFromQuantityAndCurrencyCode(1, "USD")
        };
        supplied[0].UnitPrice = 0.05m;
        supplied[1].UnitPrice = 0.05m;
        supplied[2].UnitPrice = 0.05m;
        CommerceOrder order = Fake.Create(11122).Commerce().OrderDetailsFromItems(supplied);

        decimal lineDiscount = decimal.Zero;
        decimal lineTax = decimal.Zero;
        decimal lineTotal = decimal.Zero;
        int index = 0;
        while (index < order.Items.Length)
        {
            lineDiscount += order.Items[index].DiscountAmount;
            lineTax += order.Items[index].TaxAmount;
            lineTotal += order.Items[index].Total;
            Assert.That(order.Items[index].Total, Is.EqualTo(
                order.Items[index].Subtotal - order.Items[index].DiscountAmount +
                    order.Items[index].TaxAmount));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(order.Items[0], Is.Not.SameAs(supplied[0]));
            Assert.That(supplied[0].DiscountAmount, Is.Zero);
            Assert.That(supplied[0].TaxAmount, Is.Zero);
            Assert.That(lineDiscount, Is.EqualTo(order.DiscountAmount));
            Assert.That(lineTax, Is.EqualTo(order.TaxAmount));
            Assert.That(lineTotal, Is.EqualTo(
                order.TaxableAmount + order.TaxAmount));
            Assert.That(lineTotal + order.ShippingAmount, Is.EqualTo(order.Total));
        });
    }

    [Test]
    public void SuppliedOrdersRejectEmptyMixedCurrencyAndDuplicateLinesBeforeEntropy()
    {
        CommerceLineItem usd = Fake.Create(11123).Commerce().LineItemFromQuantityAndCurrencyCode(1, "USD");
        CommerceLineItem eur = Fake.Create(11124).Commerce().LineItemFromQuantityAndCurrencyCode(1, "EUR");
        CommerceProvider inspected = Fake.Create(11125).Commerce();
        CommerceProvider untouched = Fake.Create(11125).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.OrderDetailsFromItemsAndCurrencyCode(
                    Array.Empty<CommerceLineItem>(),
                    "USD"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.OrderDetailsFromItemsAndCurrencyCode(
                    new CommerceLineItem[] { usd, eur },
                    "USD"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.OrderDetailsFromItemsAndCurrencyCode(
                    new CommerceLineItem[] { usd, usd },
                    "USD"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.OrderDetailsFromItemsAndCurrencyCode(null, "USD"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [Test]
    public void SuppliedOrdersRejectMutatedInvalidLines()
    {
        CommerceLineItem zeroQuantity = Fake.Create(11126).Commerce().LineItemFromQuantityAndCurrencyCode(1, "USD");
        CommerceLineItem zeroPrice = Fake.Create(11127).Commerce().LineItemFromQuantityAndCurrencyCode(1, "USD");
        zeroQuantity.Quantity = 0;
        zeroPrice.UnitPrice = decimal.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(() => Fake.Create(11128).Commerce().OrderDetailsFromItemsAndCurrencyCode(
                    new CommerceLineItem[] { zeroQuantity },
                    "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(11128).Commerce().OrderDetailsFromItemsAndCurrencyCode(
                    new CommerceLineItem[] { zeroPrice },
                    "USD"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(11128).Commerce().OrderDetailsFromItemsAndCurrencyCode(
                    new CommerceLineItem[] { null },
                    "USD"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void GeneratedAggregateModelsUseDistinctSkus()
    {
        CommerceProvider commerce = Fake.Create(11129).Commerce();

        int iteration = 0;
        while (iteration < 64)
        {
            CommerceOrder order = commerce.OrderDetailsFromLineCount(5);
            HashSet<string> skus = new HashSet<string>();
            int index = 0;
            while (index < order.Items.Length)
            {
                skus.Add(order.Items[index].Sku);
                index++;
            }
            Assert.That(skus.Count, Is.EqualTo(order.Items.Length));
            iteration++;
        }
    }

    [Test]
    public void OrderStatusesControlMonotonicLifecycleDates()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2034, 6, 7, 8, 0, 0, TimeSpan.Zero);
        FakeOptions options = FakeOptions.Create(11130);
        options.ReferenceTime = reference;
        CommerceProvider commerce = Fake.Create(options).Commerce();
        HashSet<CommerceOrderStatus> statuses = new HashSet<CommerceOrderStatus>();

        int index = 0;
        while (index < 128)
        {
            CommerceOrder order = commerce.OrderDetailsFromLineCount(1);
            statuses.Add(order.Status);
            Assert.That(order.CreatedAt, Is.EqualTo(reference));
            AssertOrderTimeline(order);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(statuses, Has.Count.EqualTo(5));
            Assert.That(commerce.OrderStatus(), Is.AnyOf(
                CommerceOrderStatus.Pending, CommerceOrderStatus.Confirmed,
                CommerceOrderStatus.Shipped, CommerceOrderStatus.Delivered,
                CommerceOrderStatus.Cancelled));
        });
    }

    [Test]
    public void ReturnsReferenceDeliveredOrderLinesAndCorrelateResolutionState()
    {
        CommerceProvider commerce = Fake.Create(11131).Commerce();
        HashSet<CommerceReturnStatus> statuses = new HashSet<CommerceReturnStatus>();

        int index = 0;
        while (index < 128)
        {
            CommerceReturn value = commerce.ReturnDetails();
            statuses.Add(value.Status);
            CommerceLineItem line = FindLine(value.Order.Items, value.Sku);
            Assert.Multiple(() =>
            {
                Assert.That(value.Number, Does.Match("^RET-[0-9]{8}$"));
                Assert.That(value.OrderNumber, Is.EqualTo(value.Order.Number));
                Assert.That(value.Order.Status, Is.EqualTo(CommerceOrderStatus.Delivered));
                Assert.That(value.Order.HasDeliveredAt, Is.True);
                Assert.That(line, Is.Not.Null);
                Assert.That(value.Quantity, Is.InRange(1, line.Quantity));
                Assert.That(value.CurrencyCode, Is.EqualTo(line.CurrencyCode));
                Assert.That(value.RequestedAt,
                    Is.EqualTo(value.Order.DeliveredAt.AddDays(1)));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.TestMarker,
                    Is.EqualTo("fake.synthetic-commerce-return.v1"));
                Assert.That(value.RepresentsReceivedGoods, Is.False);
                Assert.That(value.RepresentsSettledRefund, Is.False);
            });
            if (value.Status == CommerceReturnStatus.Requested)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(value.HasResolvedAt, Is.False);
                    Assert.That(value.RefundReason, Is.Empty);
                    Assert.That(value.RefundAmount, Is.Zero);
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.That(value.HasResolvedAt, Is.True);
                    Assert.That(value.ResolvedAt, Is.GreaterThan(value.RequestedAt));
                });
            }
            if (value.Status == CommerceReturnStatus.Refunded)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(value.RefundReason, Is.Not.Empty);
                    Assert.That(value.RefundAmount, Is.EqualTo(decimal.Round(
                        line.UnitPrice * value.Quantity,
                        2,
                        MidpointRounding.ToEven)));
                });
            }
            else
            {
                Assert.That(value.RefundAmount, Is.Zero);
            }
            index++;
        }

        Assert.That(statuses, Has.Count.EqualTo(4));
    }

    [Test]
    public void OrderAndReturnTimelineBoundsFailBeforeEntropy()
    {
        const long seed = 11132;
        FakeOptions options = FakeOptions.Create(seed);
        options.ReferenceTime = DateTimeOffset.MaxValue;
        CommerceProvider inspected = Fake.Create(options).Commerce();
        CommerceProvider untouched = Fake.Create(options).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.OrderDetails(), Throws.TypeOf<FakeException>());
            Assert.That(() => inspected.ReturnDetails(), Throws.TypeOf<FakeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [Test]
    public void EdgeCaseCommerceGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11133);
        Fake untouched = Fake.Create(11133);

        int index = 0;
        while (index < 16)
        {
            inspected.Commerce().OrderDetailsFromLineCount(3);
            inspected.Commerce().ReturnDetails();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertOrderTimeline(CommerceOrder order)
    {
        switch (order.Status)
        {
            case CommerceOrderStatus.Pending:
                Assert.Multiple(() =>
                {
                    Assert.That(order.HasConfirmedAt, Is.False);
                    Assert.That(order.HasShippedAt, Is.False);
                    Assert.That(order.HasDeliveredAt, Is.False);
                    Assert.That(order.HasCancelledAt, Is.False);
                });
                break;
            case CommerceOrderStatus.Confirmed:
                Assert.Multiple(() =>
                {
                    Assert.That(order.HasConfirmedAt, Is.True);
                    Assert.That(order.ConfirmedAt, Is.GreaterThan(order.CreatedAt));
                    Assert.That(order.HasShippedAt, Is.False);
                    Assert.That(order.HasDeliveredAt, Is.False);
                    Assert.That(order.HasCancelledAt, Is.False);
                });
                break;
            case CommerceOrderStatus.Shipped:
                Assert.Multiple(() =>
                {
                    Assert.That(order.HasConfirmedAt, Is.True);
                    Assert.That(order.HasShippedAt, Is.True);
                    Assert.That(order.ConfirmedAt, Is.LessThan(order.ShippedAt));
                    Assert.That(order.HasDeliveredAt, Is.False);
                    Assert.That(order.HasCancelledAt, Is.False);
                });
                break;
            case CommerceOrderStatus.Delivered:
                Assert.Multiple(() =>
                {
                    Assert.That(order.HasConfirmedAt, Is.True);
                    Assert.That(order.HasShippedAt, Is.True);
                    Assert.That(order.HasDeliveredAt, Is.True);
                    Assert.That(order.ConfirmedAt, Is.LessThan(order.ShippedAt));
                    Assert.That(order.ShippedAt, Is.LessThanOrEqualTo(order.DeliveredAt));
                    Assert.That(order.DeliveredAt,
                        Is.EqualTo(order.Shipment.EstimatedDelivery.EndsAt));
                    Assert.That(order.HasCancelledAt, Is.False);
                });
                break;
            case CommerceOrderStatus.Cancelled:
                Assert.Multiple(() =>
                {
                    Assert.That(order.HasConfirmedAt, Is.False);
                    Assert.That(order.HasShippedAt, Is.False);
                    Assert.That(order.HasDeliveredAt, Is.False);
                    Assert.That(order.HasCancelledAt, Is.True);
                    Assert.That(order.CancelledAt, Is.GreaterThan(order.CreatedAt));
                });
                break;
        }
    }

    static CommerceLineItem FindLine(CommerceLineItem[] items, string sku)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Sku == sku)
            {
                return items[index];
            }
            index++;
        }
        return null;
    }
}
