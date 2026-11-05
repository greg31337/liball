using fake;

namespace fake.Tests;

[TestFixture]
public class CompanyCommerceCompletionTests
{
    [Test]
    public void CompanySlogansAreDeterministicAuthoredCopyAndPropagateToProfiles()
    {
        const long seed = 11200;
        CompanyProvider first = Fake.Create(seed).Company();
        CompanyProvider second = Fake.Create(seed).Company();
        string firstSlogan = first.Slogan();
        string secondSlogan = second.Slogan();
        CompanyProfile firstProfile = Fake.Create(seed).Company().Profile();
        CompanyProfile secondProfile = Fake.Create(seed).Company().Profile();

        Assert.Multiple(() =>
        {
            Assert.That(firstSlogan, Is.EqualTo(secondSlogan));
            Assert.That(firstSlogan, Does.Contain(", "));
            Assert.That(firstSlogan, Does.EndWith("."));
            Assert.That(firstProfile.Slogan, Is.EqualTo(secondProfile.Slogan).And.Not.Empty);
            Assert.That(firstProfile.Slogan, Does.EndWith("."));
            Assert.That(firstProfile.IsSynthetic, Is.True);
            Assert.That(firstProfile.RepresentsRegisteredEntity, Is.False);
        });
    }

    [Test]
    public void MarketplaceSellersCorrelateIdentifiersCompanyAndReservedContacts()
    {
        const long seed = 11201;
        MarketplaceSellerRecord first = Fake.Create(seed).Commerce().MarketplaceSeller();
        MarketplaceSellerRecord second = Fake.Create(seed).Commerce().MarketplaceSeller();

        Assert.Multiple(() =>
        {
            Assert.That(first.Identifier, Does.Match("^SLR-[0-9]{8}$"));
            Assert.That(first.Identifier, Is.EqualTo(second.Identifier));
            Assert.That(first.RetailDepartment, Is.EqualTo(second.RetailDepartment).And.Not.Empty);
            Assert.That(first.DisplayName, Is.EqualTo(first.Company.Name));
            Assert.That(first.StorefrontDomain, Is.EqualTo(first.Company.Domain));
            Assert.That(first.SupportEmail, Is.EqualTo(first.Company.Email));
            Assert.That(first.StorefrontDomain, Does.EndWith(".example.com"));
            Assert.That(first.SupportEmail, Does.EndWith("@" + first.StorefrontDomain));
            Assert.That(first.Company.DomainIsReservedExample, Is.True);
            Assert.That(first.Company.EmailIsReservedExample, Is.True);
            Assert.That(first.IsSynthetic, Is.True);
            Assert.That(first.TestMarker, Is.EqualTo("fake.synthetic-marketplace.v1"));
            Assert.That(first.RepresentsActiveSeller, Is.False);
            Assert.That(first.RepresentsVerifiedSeller, Is.False);
        });
    }

    [Test]
    public void ContainerIdentifiersExposeValidCorrelatedComponentsAndRejectMutations()
    {
        const long seed = 11202;
        CommerceProvider finance = Fake.Create(seed).Commerce();
        string identifier = finance.ContainerIdentifier();
        ContainerIdentifierRecord record = Fake.Create(seed).Commerce()
            .ContainerIdentifierDetails();
        int replacement = (identifier[10] - '0' + 1) % 10;
        string mutated = identifier.Substring(0, 10) + replacement;

        Assert.Multiple(() =>
        {
            Assert.That(identifier, Does.Match("^[A-Z]{3}[UJZ][0-9]{7}$"));
            Assert.That(finance.IsContainerIdentifierValid(identifier), Is.True);
            Assert.That(finance.IsContainerIdentifierValid(mutated), Is.False);
            Assert.That(record.Identifier, Is.EqualTo(identifier));
            Assert.That(record.OwnerCode + record.EquipmentCategory +
                record.SerialNumber + record.CheckDigit, Is.EqualTo(record.Identifier));
            Assert.That(record.IsCheckDigitValid, Is.True);
            Assert.That(record.IsSynthetic, Is.True);
            Assert.That(record.TestMarker, Is.EqualTo("fake.synthetic-container.v1"));
            Assert.That(record.RepresentsRegisteredContainer, Is.False);
            Assert.That(finance.IsContainerIdentifierValid("abcU1234560"), Is.False);
            Assert.That(finance.IsContainerIdentifierValid("ABCX1234560"), Is.False);
            Assert.That(finance.IsContainerIdentifierValid("ABCU123456"), Is.False);
        });
        Assert.That(() => finance.IsContainerIdentifierValid(null),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void MarketplaceOrdersLinkSellerPurchaseOrderAndShipmentFields()
    {
        const long seed = 11203;
        CommerceOrder first = Fake.Create(seed).Commerce()
            .MarketplaceOrderFromLineCountAndCurrencyCode(3, "KWD");
        CommerceOrder second = Fake.Create(seed).Commerce()
            .MarketplaceOrderFromLineCountAndCurrencyCode(3, "KWD");
        OrderProfile profile = Fake.Create(seed).Profile().Order();

        Assert.Multiple(() =>
        {
            Assert.That(first.Number, Is.EqualTo(second.Number));
            Assert.That(first.SellerIdentifier, Is.EqualTo(second.SellerIdentifier));
            Assert.That(first.PurchaseOrderNumber, Is.EqualTo(second.PurchaseOrderNumber));
            Assert.That(first.SellerIdentifier, Is.EqualTo(first.Seller.Identifier));
            Assert.That(first.PurchaseOrderNumber, Does.Match("^PO-[0-9]{8}$"));
            Assert.That(first.Shipment.OrderNumber, Is.EqualTo(first.Number));
            Assert.That(first.Shipment.PurchaseOrderNumber,
                Is.EqualTo(first.PurchaseOrderNumber));
            Assert.That(first.Shipment.SellerIdentifier,
                Is.EqualTo(first.SellerIdentifier));
            Assert.That(first.Shipment.ContainerIdentifier,
                Is.EqualTo(first.Shipment.Container.Identifier));
            Assert.That(Fake.Create(seed).Commerce().IsContainerIdentifierValid(
                first.Shipment.ContainerIdentifier), Is.True);
            Assert.That(first.Seller.DisplayName, Is.EqualTo(first.Seller.Company.Name));
            Assert.That(first.LineCount, Is.EqualTo(3));
            Assert.That(first.CurrencyCode, Is.EqualTo("KWD"));
            Assert.That(first.Total,
                Is.EqualTo(first.TaxableAmount + first.TaxAmount + first.ShippingAmount));
            Assert.That(profile.SellerIdentifier, Is.EqualTo(profile.Seller.Identifier));
            Assert.That(profile.Shipment.OrderNumber, Is.EqualTo(profile.Number));
            Assert.That(profile.Shipment.PurchaseOrderNumber,
                Is.EqualTo(profile.PurchaseOrderNumber));
            Assert.That(profile.Shipment.SellerIdentifier,
                Is.EqualTo(profile.SellerIdentifier));
        });
    }

    [Test]
    public void StandaloneShipmentLinkageIsEmptyAndNewLimitsFailBeforeEntropy()
    {
        const long seed = 11204;
        CommerceProvider commerce = Fake.Create(seed).Commerce();
        ShipmentRecord shipment = commerce.Shipment();
        string purchaseOrderNumber = commerce.PurchaseOrderNumber();
        string retailDepartment = commerce.RetailDepartment();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 8;
        CommerceProvider limited = Fake.Create(countOptions).Commerce();
        CommerceProvider untouched = Fake.Create(countOptions).Commerce();
        FakeOptions containerOptions = FakeOptions.Create(seed);
        containerOptions.MaximumCount = 10;
        CommerceProvider containerLimited = Fake.Create(containerOptions).Commerce();
        CommerceProvider containerUntouched = Fake.Create(containerOptions).Commerce();

        Assert.Multiple(() =>
        {
            Assert.That(shipment.OrderNumber, Is.Empty);
            Assert.That(shipment.PurchaseOrderNumber, Is.Empty);
            Assert.That(shipment.SellerIdentifier, Is.Empty);
            Assert.That(shipment.ContainerIdentifier,
                Is.EqualTo(shipment.Container.Identifier));
            Assert.That(commerce.IsContainerIdentifierValid(
                shipment.ContainerIdentifier), Is.True);
            Assert.That(purchaseOrderNumber, Does.Match("^PO-[0-9]{8}$"));
            Assert.That(retailDepartment, Is.Not.Empty);
            Assert.That(() => limited.RetailDepartment(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(limited.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(() => containerLimited.ContainerIdentifier(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(containerLimited.RandomValue.Long(),
                Is.EqualTo(containerUntouched.RandomValue.Long()));
        });
    }
}
