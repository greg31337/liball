using fake;

namespace fake.Tests;

[TestFixture]
public class CompanyCommerceConformanceTests
{
    [Test]
    public void CompanyAndMarketplaceVocabularyPreserveLocaleFallbackAndInvariantCopy()
    {
        const long seed = 11210;
        MarketplaceSellerRecord french = Fake.Create("fr-BE", seed).Commerce()
            .MarketplaceSeller();
        MarketplaceSellerRecord german = Fake.Create("de-AT", seed).Commerce()
            .MarketplaceSeller();
        string englishSlogan = Fake.Create("en-US", seed).Company().Slogan();
        string japaneseSlogan = Fake.Create("ja-JP", seed).Company().Slogan();
        string englishDepartment = Fake.Create("en-US", seed).Commerce()
            .RetailDepartment();
        string japaneseDepartment = Fake.Create("ja-JP", seed).Commerce()
            .RetailDepartment();

        Assert.Multiple(() =>
        {
            Assert.That(french.Company.Locale, Is.EqualTo("fr-BE"));
            Assert.That(french.Company.CountryCode, Is.EqualTo("BE"));
            Assert.That(french.Company.Name, Does.Match(" (SARL|SAS|SA|SNC)$"));
            Assert.That(german.Company.Locale, Is.EqualTo("de-AT"));
            Assert.That(german.Company.CountryCode, Is.EqualTo("AT"));
            Assert.That(german.Company.Name,
                Does.Match(" (GmbH|AG|UG \\(haftungsbeschränkt\\))$"));
            Assert.That(japaneseSlogan, Is.EqualTo(englishSlogan));
            Assert.That(japaneseDepartment, Is.EqualTo(englishDepartment));
        });
    }

    [TestCase(SafetyMode.Reserved)]
    [TestCase(SafetyMode.SyntheticPlausible)]
    public void MarketplaceOrganizationsRetainFictionalSafetyGuarantees(
        SafetyMode safetyMode)
    {
        FakeOptions options = FakeOptions.Create("en-US", 11211);
        options.SafetyMode = safetyMode;
        MarketplaceSellerRecord seller = Fake.Create(options).Commerce()
            .MarketplaceSeller();

        Assert.Multiple(() =>
        {
            Assert.That(seller.Identifier, Does.Match("^SLR-[0-9]{8}$"));
            Assert.That(seller.Company.Name, Is.EqualTo(seller.DisplayName));
            Assert.That(seller.Company.Domain, Is.EqualTo(seller.StorefrontDomain));
            Assert.That(seller.Company.Email, Is.EqualTo(seller.SupportEmail));
            Assert.That(seller.StorefrontDomain, Does.EndWith(".example.com"));
            Assert.That(seller.SupportEmail,
                Is.EqualTo("contact@" + seller.StorefrontDomain));
            Assert.That(seller.Company.DomainIsReservedExample, Is.True);
            Assert.That(seller.Company.EmailIsReservedExample, Is.True);
            Assert.That(seller.Company.RepresentsRegisteredEntity, Is.False);
            Assert.That(seller.Company.RepresentsReachableContact, Is.False);
            Assert.That(seller.IsSynthetic, Is.True);
            Assert.That(seller.RepresentsActiveSeller, Is.False);
            Assert.That(seller.RepresentsVerifiedSeller, Is.False);
        });
    }

    [Test]
    public void EmptyAndDuplicateOrderInventoriesFailBeforeMarketplaceEntropy()
    {
        const long seed = 11212;
        CommerceLineItem line = Fake.Create(11213).Commerce()
            .LineItemFromQuantityAndCurrencyCode(1, "USD");
        CommerceProvider inspected = Fake.Create(seed).Commerce();
        CommerceProvider untouched = Fake.Create(seed).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.OrderDetailsFromItems(
                    Array.Empty<CommerceLineItem>()),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.OrderDetailsFromItems(
                    new CommerceLineItem[] { line, line }),
                Throws.TypeOf<ArgumentException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [TestCase("USD", 2)]
    [TestCase("JPY", 0)]
    [TestCase("KWD", 3)]
    public void MarketplaceOrdersReconcileArithmeticAndEveryLink(
        string currencyCode,
        int decimalPlaces)
    {
        CommerceOrder order = Fake.Create(11214).Commerce()
            .MarketplaceOrderFromLineCountAndCurrencyCode(5, currencyCode);
        decimal lineDiscount = decimal.Zero;
        decimal lineTax = decimal.Zero;
        decimal lineTotal = decimal.Zero;
        decimal subtotal = decimal.Zero;
        int quantity = 0;
        HashSet<string> skus = new HashSet<string>();
        int index = 0;
        while (index < order.Items.Length)
        {
            CommerceLineItem line = order.Items[index];
            subtotal = subtotal + line.Subtotal;
            quantity = quantity + line.Quantity;
            lineDiscount = lineDiscount + line.DiscountAmount;
            lineTax = lineTax + line.TaxAmount;
            lineTotal = lineTotal + line.Total;
            skus.Add(line.Sku);
            Assert.Multiple(() =>
            {
                Assert.That(line.CurrencyCode, Is.EqualTo(currencyCode));
                Assert.That(line.Subtotal, Is.EqualTo(decimal.Round(
                    line.UnitPrice * line.Quantity,
                    decimalPlaces,
                    MidpointRounding.ToEven)));
                Assert.That(line.Total, Is.EqualTo(
                    line.Subtotal - line.DiscountAmount + line.TaxAmount));
            });
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(order.LineCount, Is.EqualTo(5));
            Assert.That(order.Items, Has.Length.EqualTo(order.LineCount));
            Assert.That(skus, Has.Count.EqualTo(order.LineCount));
            Assert.That(order.Quantity, Is.EqualTo(quantity));
            Assert.That(order.Subtotal, Is.EqualTo(subtotal));
            Assert.That(order.DiscountAmount, Is.EqualTo(lineDiscount));
            Assert.That(order.TaxAmount, Is.EqualTo(lineTax));
            Assert.That(lineTotal,
                Is.EqualTo(order.TaxableAmount + order.TaxAmount));
            Assert.That(order.Total,
                Is.EqualTo(lineTotal + order.ShippingAmount));
            Assert.That(order.SellerIdentifier, Is.EqualTo(order.Seller.Identifier));
            Assert.That(order.Shipment.OrderNumber, Is.EqualTo(order.Number));
            Assert.That(order.Shipment.PurchaseOrderNumber,
                Is.EqualTo(order.PurchaseOrderNumber));
            Assert.That(order.Shipment.SellerIdentifier,
                Is.EqualTo(order.SellerIdentifier));
        });
    }

    [Test]
    public void MarketplaceShipmentRoutesContainersAndDeliveryWindowsStayOrdered()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2037, 3, 4, 5, 0, 0, TimeSpan.Zero);
        FakeOptions options = FakeOptions.Create(11215);
        options.ReferenceTime = reference;
        CommerceProvider commerce = Fake.Create(options).Commerce();
        HashSet<CommerceOrderStatus> statuses = new HashSet<CommerceOrderStatus>();

        int index = 0;
        while (index < 128)
        {
            CommerceOrder order = commerce.MarketplaceOrder();
            ShipmentRecord shipment = order.Shipment;
            statuses.Add(order.Status);
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
                Assert.That(shipment.EstimatedDelivery.ReferenceTime,
                    Is.EqualTo(reference));
                Assert.That(shipment.EstimatedDelivery.StartsAt,
                    Is.EqualTo(reference.AddDays(
                        shipment.Service.MinimumTransitDays)));
                Assert.That(shipment.EstimatedDelivery.EndsAt,
                    Is.EqualTo(reference.AddDays(
                        shipment.Service.MaximumTransitDays)));
                Assert.That(shipment.EstimatedDelivery.StartsAt,
                    Is.LessThanOrEqualTo(shipment.EstimatedDelivery.EndsAt));
                Assert.That(shipment.ContainerIdentifier,
                    Is.EqualTo(shipment.Container.Identifier));
                Assert.That(commerce.IsContainerIdentifierValid(
                    shipment.ContainerIdentifier), Is.True);
            });
            if (order.Status == CommerceOrderStatus.Delivered)
            {
                Assert.That(order.DeliveredAt,
                    Is.EqualTo(shipment.EstimatedDelivery.EndsAt));
            }
            index = index + 1;
        }

        Assert.That(statuses, Has.Count.EqualTo(5));
    }

    [Test]
    public void ReturnAndRefundStatesPreserveMarketplaceOrderCorrelation()
    {
        CommerceProvider commerce = Fake.Create(11216).Commerce();
        HashSet<CommerceReturnStatus> statuses = new HashSet<CommerceReturnStatus>();

        int index = 0;
        while (index < 128)
        {
            CommerceReturn value = commerce.ReturnDetails();
            CommerceLineItem line = FindLine(value.Order.Items, value.Sku);
            statuses.Add(value.Status);
            Assert.Multiple(() =>
            {
                Assert.That(value.OrderNumber, Is.EqualTo(value.Order.Number));
                Assert.That(value.Order.Status,
                    Is.EqualTo(CommerceOrderStatus.Delivered));
                Assert.That(value.Order.SellerIdentifier,
                    Is.EqualTo(value.Order.Seller.Identifier));
                Assert.That(value.Order.Shipment.OrderNumber,
                    Is.EqualTo(value.Order.Number));
                Assert.That(value.Order.Shipment.PurchaseOrderNumber,
                    Is.EqualTo(value.Order.PurchaseOrderNumber));
                Assert.That(value.Order.Shipment.SellerIdentifier,
                    Is.EqualTo(value.Order.SellerIdentifier));
                Assert.That(value.RequestedAt,
                    Is.GreaterThan(value.Order.DeliveredAt));
                Assert.That(value.Quantity, Is.InRange(1, line.Quantity));
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
                Assert.Multiple(() =>
                {
                    Assert.That(value.RefundReason, Is.Empty);
                    Assert.That(value.RefundAmount, Is.Zero);
                });
            }
            index = index + 1;
        }

        Assert.That(statuses, Has.Count.EqualTo(4));
    }

    [Test]
    public void ContainerRecordsUseIndependentChecksumsAndCoverAuthoredShapes()
    {
        CommerceProvider commerce = Fake.Create(11217).Commerce();
        HashSet<string> owners = new HashSet<string>();
        HashSet<string> categories = new HashSet<string>();

        int index = 0;
        while (index < 128)
        {
            ContainerIdentifierRecord record = commerce.ContainerIdentifierDetails();
            owners.Add(record.OwnerCode);
            categories.Add(record.EquipmentCategory);
            int expectedCheckDigit = CalculateContainerCheckDigit(
                record.Identifier.Substring(0, 10));
            Assert.Multiple(() =>
            {
                Assert.That(record.Identifier, Is.EqualTo(
                    record.OwnerCode + record.EquipmentCategory +
                        record.SerialNumber + record.CheckDigit));
                Assert.That(record.CheckDigit, Is.EqualTo(expectedCheckDigit));
                Assert.That(commerce.IsContainerIdentifierValid(record.Identifier),
                    Is.True);
            });
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(owners, Is.EquivalentTo(
                new string[] { "QXZ", "VQX", "XQZ", "ZXQ" }));
            Assert.That(categories, Is.EquivalentTo(
                new string[] { "U", "J", "Z" }));
        });
    }

    [Test]
    public void MarketplaceOrdersAreDeterministicAcrossEveryNewCorrelation()
    {
        const long seed = 11218;
        CommerceOrder first = Fake.Create(seed).Commerce().MarketplaceOrder();
        CommerceOrder second = Fake.Create(seed).Commerce().MarketplaceOrder();
        CommerceOrder ordinary = Fake.Create(seed).Commerce().OrderDetails();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Number, Is.EqualTo(first.Number));
            Assert.That(second.Total, Is.EqualTo(first.Total));
            Assert.That(second.Seller.Identifier, Is.EqualTo(first.Seller.Identifier));
            Assert.That(second.Seller.RetailDepartment,
                Is.EqualTo(first.Seller.RetailDepartment));
            Assert.That(second.Seller.Company.Slogan,
                Is.EqualTo(first.Seller.Company.Slogan));
            Assert.That(second.PurchaseOrderNumber,
                Is.EqualTo(first.PurchaseOrderNumber));
            Assert.That(second.Shipment.ContainerIdentifier,
                Is.EqualTo(first.Shipment.ContainerIdentifier));
            Assert.That(second.Shipment.Route.Label,
                Is.EqualTo(first.Shipment.Route.Label));
            Assert.That(second.Shipment.EstimatedDelivery.EndsAt,
                Is.EqualTo(first.Shipment.EstimatedDelivery.EndsAt));
            Assert.That(ordinary.Number, Is.EqualTo(first.Number));
            Assert.That(ordinary.SellerIdentifier,
                Is.EqualTo(first.SellerIdentifier));
            Assert.That(ordinary.PurchaseOrderNumber,
                Is.EqualTo(first.PurchaseOrderNumber));
            Assert.That(ordinary.Shipment.ContainerIdentifier,
                Is.EqualTo(first.Shipment.ContainerIdentifier));
        });
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
            index = index + 1;
        }
        throw new InvalidOperationException("The return SKU was not found in its order.");
    }

    static int CalculateContainerCheckDigit(string body)
    {
        int sum = 0;
        int weight = 1;
        int index = 0;
        while (index < body.Length)
        {
            int value = body[index] >= '0' && body[index] <= '9'
                ? body[index] - '0'
                : ContainerLetterValue(body[index]);
            sum = sum + value * weight;
            weight = weight * 2;
            index = index + 1;
        }
        int remainder = sum % 11;
        return remainder == 10 ? 0 : remainder;
    }

    static int ContainerLetterValue(char value)
    {
        int result = value - 'A' + 10;
        if (result >= 11)
        {
            result = result + 1;
        }
        if (result >= 22)
        {
            result = result + 1;
        }
        if (result >= 33)
        {
            result = result + 1;
        }
        return result;
    }
}
