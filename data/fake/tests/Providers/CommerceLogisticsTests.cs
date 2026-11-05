using fake;

namespace fake.Tests;

[TestFixture]
public class CommerceLogisticsTests
{
    [Test]
    public void ShippingServicesUseAuthoredCarriersAndMethodTransitBounds()
    {
        CommerceProvider commerce = Fake.Create(11110).Commerce();

        int index = 0;
        while (index < 128)
        {
            ShippingService service = commerce.ShippingService();
            Assert.Multiple(() =>
            {
                Assert.That(service.Carrier, Is.AnyOf(
                    "Cedar Parcel", "Harbor Dispatch", "Northbridge Freight",
                    "Summit Courier"));
                AssertService(service);
                Assert.That(service.IsSynthetic, Is.True);
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(commerce.Carrier(), Is.AnyOf(
                "Cedar Parcel", "Harbor Dispatch", "Northbridge Freight",
                "Summit Courier"));
            Assert.That(commerce.ShippingMethod(), Is.AnyOf(
                "Economy Ground", "Standard Ground", "Two-Day", "Next-Day"));
        });
    }

    [Test]
    public void WarehousesHaveAddressCorrelatedCodesAndBins()
    {
        const long seed = 11111;
        WarehouseRecord first = Fake.Create(seed).Commerce().Warehouse();
        WarehouseRecord second = Fake.Create(seed).Commerce().Warehouse();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Code, Is.EqualTo(first.Code));
            Assert.That(second.Name, Is.EqualTo(first.Name));
            Assert.That(second.Bin, Is.EqualTo(first.Bin));
            Assert.That(first.Code,
                Does.Match("^WH-" + first.Address.CountryCode + "-[0-9]{3}$"));
            Assert.That(first.Bin, Does.Match("^[A-Z]-[0-9]{2}-[0-9]{3}$"));
            Assert.That(first.Name, Is.AnyOf(
                "Cedar Depot", "Harbor Storehouse", "Northbridge Warehouse",
                "Silver Meadow Depot", "Summit Distribution"));
            Assert.That(first.Address.NonDeliverabilityGuaranteed, Is.True);
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.RepresentsRealFacility, Is.False);
            Assert.That(Fake.Create(seed).Commerce().WarehouseBin(),
                Does.Match("^[A-Z]-[0-9]{2}-[0-9]{3}$"));
        });
    }

    [Test]
    public void PackagesHaveBoundedMeasurementsAndSyntheticIdentifiers()
    {
        CommerceProvider commerce = Fake.Create(11112).Commerce();

        int index = 0;
        while (index < 128)
        {
            PackageRecord package = commerce.Package();
            Assert.Multiple(() =>
            {
                Assert.That(package.Identifier,
                    Does.Match("^PKG-[A-Za-z0-9]{10}$"));
                Assert.That(package.Type, Is.AnyOf("Box", "Crate", "Envelope", "Tube"));
                Assert.That(package.Dimensions.Length, Is.InRange(1m, 200m));
                Assert.That(package.Dimensions.Width, Is.InRange(1m, 200m));
                Assert.That(package.Dimensions.Height, Is.InRange(1m, 200m));
                Assert.That(package.Dimensions.Volume, Is.EqualTo(decimal.Round(
                    package.Dimensions.Length * package.Dimensions.Width *
                        package.Dimensions.Height,
                    3)));
                Assert.That(package.Dimensions.Unit, Is.EqualTo("centimeters"));
                Assert.That(package.Weight, Is.InRange(0.05m, 50m));
                Assert.That(package.IsSynthetic, Is.True);
            });
            index++;
        }
    }

    [Test]
    public void RoutesAndDeliveryIntervalsPreserveTheirDeclaredCorrelations()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2032, 4, 5, 12, 30, 0, TimeSpan.Zero);
        FakeOptions options = FakeOptions.Create(11113);
        options.ReferenceTime = reference;
        CommerceProvider commerce = Fake.Create(options).Commerce();

        int index = 0;
        while (index < 128)
        {
            ShippingRoute route = commerce.Route();
            DeliveryInterval interval = commerce.DeliveryInterval();
            Assert.Multiple(() =>
            {
                Assert.That(route.OriginWarehouseCode, Does.StartWith("WH-"));
                Assert.That(route.Label, Is.EqualTo(
                    route.OriginWarehouseCode + ">" +
                    route.DestinationSubdivisionCode));
                Assert.That(route.IsInternational, Is.EqualTo(
                    route.OriginCountryCode != route.DestinationCountryCode));
                Assert.That(interval.ReferenceTime, Is.EqualTo(reference));
                Assert.That(interval.StartsAt,
                    Is.EqualTo(reference.AddDays(interval.MinimumTransitDays)));
                Assert.That(interval.EndsAt,
                    Is.EqualTo(reference.AddDays(interval.MaximumTransitDays)));
                Assert.That(interval.StartsAt, Is.LessThanOrEqualTo(interval.EndsAt));
                Assert.That(interval.IsGuaranteed, Is.False);
                AssertDeliveryBounds(interval);
            });
            index++;
        }
    }

    [Test]
    public void ShipmentsCorrelateAddressesWarehouseRouteServiceAndDelivery()
    {
        CommerceProvider commerce = Fake.Create(11114).Commerce();
        bool sawMatching = false;
        bool sawDistinct = false;

        int index = 0;
        while (index < 128)
        {
            ShipmentRecord shipment = commerce.Shipment();
            if (shipment.BillingMatchesShipping)
            {
                sawMatching = true;
                Assert.That(shipment.BillingAddress,
                    Is.SameAs(shipment.ShippingAddress));
            }
            else
            {
                sawDistinct = true;
                Assert.That(shipment.BillingAddress,
                    Is.Not.SameAs(shipment.ShippingAddress));
            }
            Assert.Multiple(() =>
            {
                Assert.That(shipment.Route.OriginWarehouseCode,
                    Is.EqualTo(shipment.Warehouse.Code));
                Assert.That(shipment.Route.OriginCountryCode,
                    Is.EqualTo(shipment.Warehouse.Address.CountryCode));
                Assert.That(shipment.Route.DestinationCountryCode,
                    Is.EqualTo(shipment.ShippingAddress.CountryCode));
                Assert.That(shipment.Route.DestinationSubdivisionCode,
                    Is.EqualTo(shipment.ShippingAddress.SubdivisionCode));
                Assert.That(shipment.Route.DestinationPostalCode,
                    Is.EqualTo(shipment.ShippingAddress.PostalCode));
                Assert.That(shipment.EstimatedDelivery.Method,
                    Is.EqualTo(shipment.Service.Method));
                Assert.That(shipment.EstimatedDelivery.MinimumTransitDays,
                    Is.EqualTo(shipment.Service.MinimumTransitDays));
                Assert.That(shipment.EstimatedDelivery.MaximumTransitDays,
                    Is.EqualTo(shipment.Service.MaximumTransitDays));
                Assert.That(shipment.TrackingNumber,
                    Does.Match("^TRK-[A-Za-z0-9]{16}$"));
                Assert.That(shipment.IsSynthetic, Is.True);
                Assert.That(shipment.TestMarker,
                    Is.EqualTo("fake.synthetic-shipment.v1"));
                Assert.That(shipment.RepresentsCarrierAcceptance, Is.False);
                Assert.That(shipment.RepresentsDeliverableShipment, Is.False);
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawMatching, Is.True);
            Assert.That(sawDistinct, Is.True);
        });
    }

    [Test]
    public void OrdersAndInvoicesExposeTheirCorrelatedShipmentAddresses()
    {
        CommerceOrder order = Fake.Create(11115).Commerce().OrderDetailsFromLineCount(2);
        CommerceInvoice invoice = Fake.Create(11116).Commerce().InvoiceDetailsFromLineCount(2);

        Assert.Multiple(() =>
        {
            Assert.That(order.Shipment, Is.Not.Null);
            Assert.That(order.ShippingAddress,
                Is.SameAs(order.Shipment.ShippingAddress));
            Assert.That(order.BillingAddress,
                Is.SameAs(order.Shipment.BillingAddress));
            Assert.That(order.BillingMatchesShipping,
                Is.EqualTo(order.Shipment.BillingMatchesShipping));
            Assert.That(invoice.Shipment, Is.Not.Null);
            Assert.That(invoice.ShippingAddress,
                Is.SameAs(invoice.Shipment.ShippingAddress));
            Assert.That(invoice.BillingAddress,
                Is.SameAs(invoice.Shipment.BillingAddress));
            Assert.That(invoice.BillingMatchesShipping,
                Is.EqualTo(invoice.Shipment.BillingMatchesShipping));
        });
    }

    [Test]
    public void LogisticsFixturesAreDeterministicNewInstances()
    {
        const long seed = 11117;
        ShipmentRecord first = Fake.Create(seed).Commerce().Shipment();
        ShipmentRecord second = Fake.Create(seed).Commerce().Shipment();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.TrackingNumber, Is.EqualTo(first.TrackingNumber));
            Assert.That(second.Service.Carrier, Is.EqualTo(first.Service.Carrier));
            Assert.That(second.Service.Method, Is.EqualTo(first.Service.Method));
            Assert.That(second.Warehouse.Code, Is.EqualTo(first.Warehouse.Code));
            Assert.That(second.Package.Identifier, Is.EqualTo(first.Package.Identifier));
            Assert.That(second.Route.Label, Is.EqualTo(first.Route.Label));
            Assert.That(second.EstimatedDelivery.StartsAt,
                Is.EqualTo(first.EstimatedDelivery.StartsAt));
            Assert.That(second.EstimatedDelivery.EndsAt,
                Is.EqualTo(first.EstimatedDelivery.EndsAt));
        });
    }

    [Test]
    public void LogisticsLimitsAndReferenceBoundsFailBeforeEntropy()
    {
        const long seed = 11118;
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        CommerceProvider limited = Fake.Create(countOptions).Commerce();
        CommerceProvider limitedUntouched = Fake.Create(countOptions).Commerce();
        FakeOptions dateOptions = FakeOptions.Create(seed);
        dateOptions.ReferenceTime = DateTimeOffset.MaxValue;
        CommerceProvider dated = Fake.Create(dateOptions).Commerce();
        CommerceProvider datedUntouched = Fake.Create(dateOptions).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => limited.ShippingService(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(limited.RandomValue.Long(),
                Is.EqualTo(limitedUntouched.RandomValue.Long()));
            Assert.That(() => dated.DeliveryInterval(),
                Throws.TypeOf<FakeException>());
            Assert.That(() => dated.Shipment(),
                Throws.TypeOf<FakeException>());
            Assert.That(dated.RandomValue.Long(),
                Is.EqualTo(datedUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void LogisticsGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11119);
        Fake untouched = Fake.Create(11119);

        int index = 0;
        while (index < 16)
        {
            inspected.Commerce().ShippingService();
            inspected.Commerce().Warehouse();
            inspected.Commerce().Package();
            inspected.Commerce().Route();
            inspected.Commerce().DeliveryInterval();
            inspected.Commerce().Shipment();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertService(ShippingService service)
    {
        switch (service.Method)
        {
            case "Economy Ground":
                Assert.That(service.MinimumTransitDays, Is.EqualTo(5));
                Assert.That(service.MaximumTransitDays, Is.EqualTo(8));
                break;
            case "Standard Ground":
                Assert.That(service.MinimumTransitDays, Is.EqualTo(3));
                Assert.That(service.MaximumTransitDays, Is.EqualTo(5));
                break;
            case "Two-Day":
                Assert.That(service.MinimumTransitDays, Is.EqualTo(2));
                Assert.That(service.MaximumTransitDays, Is.EqualTo(2));
                break;
            case "Next-Day":
                Assert.That(service.MinimumTransitDays, Is.EqualTo(1));
                Assert.That(service.MaximumTransitDays, Is.EqualTo(1));
                break;
            default:
                Assert.Fail("Unexpected shipping method: " + service.Method);
                break;
        }
    }

    static void AssertDeliveryBounds(DeliveryInterval interval)
    {
        ShippingService service = new ShippingService();
        service.Method = interval.Method;
        service.MinimumTransitDays = interval.MinimumTransitDays;
        service.MaximumTransitDays = interval.MaximumTransitDays;
        AssertService(service);
    }
}
