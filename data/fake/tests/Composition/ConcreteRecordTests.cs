using fake;

namespace fake.Tests;

[TestFixture]
public class ConcreteRecordTests
{
    [Test]
    public void OrderProfilePreservesGeneratedOrderCorrelations()
    {
        FakeOptions options = FakeOptions.Create(13300);
        options.ReferenceTime = new DateTimeOffset(
            2043,
            4,
            5,
            6,
            7,
            8,
            TimeSpan.Zero);
        CommerceOrder source = Fake.Create(options).Commerce().OrderDetailsFromLineCountAndCurrencyCode(
            3,
            "KWD");
        OrderProfile profile = OrderProfile.FromCommerceOrder(source);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Number, Is.EqualTo(source.Number));
            Assert.That(profile.Items, Has.Length.EqualTo(3));
            Assert.That(profile.LineCount, Is.EqualTo(source.LineCount));
            Assert.That(profile.Quantity, Is.EqualTo(source.Quantity));
            Assert.That(profile.CurrencyCode,
                Is.EqualTo(source.CurrencyCode));
            Assert.That(profile.Subtotal, Is.EqualTo(source.Subtotal));
            Assert.That(profile.DiscountRate,
                Is.EqualTo(source.DiscountRate));
            Assert.That(profile.DiscountAmount,
                Is.EqualTo(source.DiscountAmount));
            Assert.That(profile.TaxableAmount,
                Is.EqualTo(source.TaxableAmount));
            Assert.That(profile.TaxRate, Is.EqualTo(source.TaxRate));
            Assert.That(profile.TaxAmount, Is.EqualTo(source.TaxAmount));
            Assert.That(profile.ShippingAmount,
                Is.EqualTo(source.ShippingAmount));
            Assert.That(profile.Total, Is.EqualTo(source.Total));
            Assert.That(profile.ShippingAddress,
                Is.SameAs(source.ShippingAddress));
            Assert.That(profile.BillingAddress,
                Is.SameAs(source.BillingAddress));
            Assert.That(profile.BillingMatchesShipping,
                Is.EqualTo(source.BillingMatchesShipping));
            Assert.That(profile.Shipment, Is.SameAs(source.Shipment));
            Assert.That(profile.Status, Is.EqualTo(source.Status));
            Assert.That(profile.CreatedAt, Is.EqualTo(source.CreatedAt));
            Assert.That(profile.HasConfirmedAt,
                Is.EqualTo(source.HasConfirmedAt));
            Assert.That(profile.ConfirmedAt,
                Is.EqualTo(source.ConfirmedAt));
            Assert.That(profile.HasShippedAt,
                Is.EqualTo(source.HasShippedAt));
            Assert.That(profile.ShippedAt, Is.EqualTo(source.ShippedAt));
            Assert.That(profile.HasDeliveredAt,
                Is.EqualTo(source.HasDeliveredAt));
            Assert.That(profile.DeliveredAt,
                Is.EqualTo(source.DeliveredAt));
            Assert.That(profile.HasCancelledAt,
                Is.EqualTo(source.HasCancelledAt));
            Assert.That(profile.CancelledAt,
                Is.EqualTo(source.CancelledAt));
            Assert.That(profile.IsSynthetic, Is.True);
            Assert.That(profile.TestMarker,
                Is.EqualTo(source.TestMarker).And.Not.Empty);
            Assert.That(profile.IsSubmitted, Is.False);
        });

        decimal subtotal = decimal.Zero;
        decimal discount = decimal.Zero;
        decimal tax = decimal.Zero;
        decimal total = decimal.Zero;
        int quantity = 0;
        int index = 0;
        while (index < profile.Items.Length)
        {
            AssertLine(profile.Items[index], source.Items[index]);
            subtotal += profile.Items[index].Subtotal;
            discount += profile.Items[index].DiscountAmount;
            tax += profile.Items[index].TaxAmount;
            total += profile.Items[index].Total;
            quantity += profile.Items[index].Quantity;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(subtotal, Is.EqualTo(profile.Subtotal));
            Assert.That(discount, Is.EqualTo(profile.DiscountAmount));
            Assert.That(tax, Is.EqualTo(profile.TaxAmount));
            Assert.That(total + profile.ShippingAmount,
                Is.EqualTo(profile.Total));
            Assert.That(quantity, Is.EqualTo(profile.Quantity));
        });
    }

    [Test]
    public void PaymentProfilePreservesGeneratedPaymentCorrelations()
    {
        FakeOptions options = FakeOptions.Create(13301);
        options.ReferenceTime = new DateTimeOffset(
            2044,
            5,
            6,
            7,
            8,
            9,
            TimeSpan.Zero);
        PaymentRecord source = Fake.Create(options).Finance().PaymentFromCurrencyCode("KWD");
        PaymentProfile profile = PaymentProfile.FromPaymentRecord(source);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Reference, Is.EqualTo(source.Reference));
            Assert.That(profile.InvoiceNumber,
                Is.EqualTo(source.InvoiceNumber));
            Assert.That(profile.Type, Is.EqualTo(source.Type));
            Assert.That(profile.Description,
                Is.EqualTo(source.Description));
            Assert.That(profile.CheckNumber,
                Is.EqualTo(source.CheckNumber));
            Assert.That(profile.CurrencyCode, Is.EqualTo("KWD"));
            Assert.That(profile.Subtotal, Is.EqualTo(source.Subtotal));
            Assert.That(profile.TaxRate, Is.EqualTo(source.TaxRate));
            Assert.That(profile.TaxAmount, Is.EqualTo(source.TaxAmount));
            Assert.That(profile.DiscountRate,
                Is.EqualTo(source.DiscountRate));
            Assert.That(profile.DiscountAmount,
                Is.EqualTo(source.DiscountAmount));
            Assert.That(profile.FeeAmount, Is.EqualTo(source.FeeAmount));
            Assert.That(profile.Amount, Is.EqualTo(source.Amount));
            Assert.That(profile.BalanceBefore,
                Is.EqualTo(source.BalanceBefore));
            Assert.That(profile.BalanceAfter,
                Is.EqualTo(source.BalanceAfter));
            Assert.That(profile.BalanceAfter,
                Is.EqualTo(profile.BalanceBefore - profile.Amount));
            Assert.That(profile.OccurredOn, Is.EqualTo(source.OccurredOn));
            Assert.That(profile.IsSynthetic, Is.True);
            Assert.That(profile.TestMarker,
                Is.EqualTo(source.TestMarker).And.Not.Empty);
            Assert.That(profile.IsAuthorized, Is.False);
            Assert.That(profile.IsSettled, Is.False);
        });
    }

    [Test]
    public void DeviceProfilePreservesGeneratedDeviceCorrelations()
    {
        DeviceRecord source = Fake.Create(13302).Device().Record();
        DeviceProfile profile = DeviceProfile.FromDeviceRecord(source);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Manufacturer,
                Is.EqualTo(source.Manufacturer));
            Assert.That(profile.Model, Is.EqualTo(source.Model));
            Assert.That(profile.Type, Is.EqualTo(source.Type));
            Assert.That(profile.OperatingSystem,
                Is.EqualTo(source.OperatingSystem));
            Assert.That(profile.OperatingSystemVersion,
                Is.EqualTo(source.OperatingSystemVersion));
            Assert.That(profile.ScreenWidth,
                Is.EqualTo(source.ScreenWidth));
            Assert.That(profile.ScreenHeight,
                Is.EqualTo(source.ScreenHeight));
            Assert.That(profile.ScreenResolution,
                Is.EqualTo(source.ScreenResolution));
            Assert.That(profile.ScreenResolution, Is.EqualTo(
                profile.ScreenWidth + "x" + profile.ScreenHeight));
            Assert.That(profile.HardwareToken,
                Is.EqualTo(source.HardwareToken));
            Assert.That(profile.UsesAndroid,
                Is.EqualTo(profile.OperatingSystem == "Android"));
            Assert.That(profile.AndroidId, Is.EqualTo(source.AndroidId));
            Assert.That(profile.PushToken, Is.EqualTo(source.PushToken));
        });
    }

    [Test]
    public void IntervalRecordPreservesOrderedGeneratedBounds()
    {
        DateTimeOffset minimum = new DateTimeOffset(
            2030,
            1,
            2,
            3,
            4,
            5,
            TimeSpan.Zero);
        DateTimeOffset maximum = minimum.AddDays(30);
        DateInterval source = Fake.Create(13303).Date().OrderedInterval(
            minimum,
            maximum);
        IntervalRecord record = IntervalRecord.FromDateInterval(source);

        Assert.Multiple(() =>
        {
            Assert.That(record.Start, Is.EqualTo(source.Start));
            Assert.That(record.End, Is.EqualTo(source.End));
            Assert.That(record.Start, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(record.End, Is.LessThan(maximum));
            Assert.That(record.End, Is.GreaterThanOrEqualTo(record.Start));
        });
    }

    static void AssertLine(
        OrderLineRecord profile,
        CommerceLineItem source)
    {
        Assert.Multiple(() =>
        {
            Assert.That(profile.Sku, Is.EqualTo(source.Sku));
            Assert.That(profile.ProductName,
                Is.EqualTo(source.ProductName));
            Assert.That(profile.Quantity, Is.EqualTo(source.Quantity));
            Assert.That(profile.UnitPrice, Is.EqualTo(source.UnitPrice));
            Assert.That(profile.Subtotal, Is.EqualTo(source.Subtotal));
            Assert.That(profile.DiscountAmount,
                Is.EqualTo(source.DiscountAmount));
            Assert.That(profile.TaxAmount, Is.EqualTo(source.TaxAmount));
            Assert.That(profile.Total, Is.EqualTo(source.Total));
            Assert.That(profile.CurrencyCode,
                Is.EqualTo(source.CurrencyCode));
            Assert.That(profile.IsSynthetic, Is.True);
            Assert.That(profile.TestMarker,
                Is.EqualTo(source.TestMarker).And.Not.Empty);
            Assert.That(profile.Total, Is.EqualTo(
                profile.Subtotal - profile.DiscountAmount +
                profile.TaxAmount));
        });
    }
}
